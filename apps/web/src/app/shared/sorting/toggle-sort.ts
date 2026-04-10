/** Toggle sort column/direction for list pages (first click on a column → asc, same column again → flip). */
export function toggleSort(
  column: string,
  sortBy: string,
  sortDirection: 'asc' | 'desc'
): { sortBy: string; sortDirection: 'asc' | 'desc' } {
  if (sortBy === column) {
    return { sortBy, sortDirection: sortDirection === 'asc' ? 'desc' : 'asc' };
  }
  return { sortBy: column, sortDirection: 'asc' };
}
